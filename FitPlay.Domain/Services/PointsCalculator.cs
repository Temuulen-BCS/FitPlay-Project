using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitPlay.Domain.Services;

public static class PointsCalculator
{
    public static int Calculate(int basePoints, int difficulty, int actualDurationMin, int targetDurationMin)
    {
        double difficultyMultiplier = 0.8 + 0.2 * difficulty; // 1→1.0, 5→1.8
        double ratio = targetDurationMin > 0 ? (double)actualDurationMin / targetDurationMin : 1.0;
        double durationFactor = Math.Clamp(ratio, 0.5, 1.5);
        return (int)Math.Round(basePoints * difficultyMultiplier * durationFactor);
    }
}
