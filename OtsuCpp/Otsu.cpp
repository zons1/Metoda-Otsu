#include "pch.h"
#include <cstdint>
#include <thread>
#include <vector>

namespace
{
    // Szaroœæ na liczbach ca³kowitych: wagi 29+150+77 = 256, dzielenie przez przesuniêcie.
    // Piksel w pamiêci to B, G, R.
    inline int Gray(const uint8_t* p)
    {
        return (29 * p[0] + 150 * p[1] + 77 * p[2]) >> 8;
    }

    // Histogram wierszy [y0, y1). Licznik lokalny na stosie w¹tku, na koñcu kopia do jego slotu.
    void HistogramRows(const uint8_t* data, int width, int stride, int y0, int y1, uint32_t* out)
    {
        uint32_t hist[256] = {};
        for (int y = y0; y < y1; y++)
        {
            const uint8_t* p = data + (long long)y * stride;
            for (int x = 0; x < width; x++, p += 3)
                hist[Gray(p)]++;
        }
        for (int i = 0; i < 256; i++) out[i] = hist[i];
    }

    // Binaryzacja wierszy [y0, y1): jasnoœæ > próg -> bia³y, w przeciwnym razie czarny.
    void ThresholdRows(uint8_t* data, int width, int stride, int y0, int y1, int t)
    {
        for (int y = y0; y < y1; y++)
        {
            uint8_t* p = data + (long long)y * stride;
            for (int x = 0; x < width; x++, p += 3)
            {
                uint8_t v = Gray(p) > t ? 255 : 0;
                p[0] = p[1] = p[2] = v;
            }
        }
    }

    // Próg Otsu: maksymalizacja wariancji miêdzyklasowej (tylko 256 iteracji).
    int OtsuThreshold(const uint32_t* h, uint64_t total)
    {
        uint64_t sumAll = 0;
        for (int i = 0; i < 256; i++) sumAll += (uint64_t)i * h[i];

        uint64_t wB = 0, sumB = 0;
        double best = -1.0;
        int bestT = 0;
        for (int t = 0; t < 256; t++)
        {
            wB += h[t];
            if (wB == 0) continue;
            uint64_t wF = total - wB;
            if (wF == 0) break;
            sumB += (uint64_t)t * h[t];
            double mB = (double)sumB / (double)wB;
            double mF = (double)(sumAll - sumB) / (double)wF;
            double between = (double)wB * (double)wF * (mB - mF) * (mB - mF);
            if (between > best) { best = between; bestT = t; }
        }
        return bestT;
    }

    // Uruchamia fn(i) dla i = 0..threads-1; w¹tek 0 to w¹tek wywo³uj¹cy.
    template <typename F>
    void RunParallel(int threads, const F& fn)
    {
        std::vector<std::thread> th;
        for (int i = 1; i < threads; i++)
            th.emplace_back([&fn, i] { fn(i); });
        fn(0);
        for (auto& t : th) t.join();
    }
}

// data: surowe dane BGR (z wyrównaniem wierszy), stride: bajtów na wiersz.
// Zwraca wyznaczony próg (0-255) lub -1 przy b³êdnych parametrach.
extern "C" __declspec(dllexport) int __cdecl OtsuProcess(
    uint8_t* data, int width, int height, int stride, int threads)
{
    if (!data || width <= 0 || height <= 0 || threads < 1) return -1;
    if (threads > 64) threads = 64;
    if (threads > height) threads = height;

    // Zrównowa¿ony podzia³ wierszy: pierwsze (height % threads) w¹tków dostaje o 1 wiersz wiêcej.
    std::vector<int> rows(threads + 1);
    int base = height / threads, extra = height % threads;
    rows[0] = 0;
    for (int i = 0; i < threads; i++)
        rows[i + 1] = rows[i] + base + (i < extra ? 1 : 0);

    // Etap 1: histogramy czêœciowe
    std::vector<uint32_t> partial((size_t)threads * 256);
    RunParallel(threads, [&](int i)
        {
            HistogramRows(data, width, stride, rows[i], rows[i + 1], &partial[(size_t)i * 256]);
        });

    // Scalenie i próg
    uint32_t hist[256] = {};
    for (int i = 0; i < threads; i++)
        for (int b = 0; b < 256; b++)
            hist[b] += partial[(size_t)i * 256 + b];
    int t = OtsuThreshold(hist, (uint64_t)width * (uint64_t)height);

    // Etap 2: binaryzacja
    RunParallel(threads, [&](int i)
        {
            ThresholdRows(data, width, stride, rows[i], rows[i + 1], t);
        });

    return t;
}