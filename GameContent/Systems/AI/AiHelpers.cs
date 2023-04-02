using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TanksRebirth.GameContent.ID;

namespace TanksRebirth.GameContent; 

public static class AiHelpers {
    public static int GetHighestTierActive() {
        var highest = TankID.None;
        
        Span<AITank> tanks = GameHandler.AllAITanks;
        ref var tanksSearchSpace = ref MemoryMarshal.GetReference(tanks);
        
        for (var i = 0; i < GameHandler.AllAITanks.Length; i++) {
            var tank = Unsafe.Add(ref tanksSearchSpace, i);

            if (tank is null || tank.Dead) continue;
            
            if (tank.AiTankType > highest)
                highest = tank.AiTankType;
        }

        return highest;
    }

    public static int CountAll() {
        var cnt = 0;
        Span<AITank> tanks = GameHandler.AllAITanks;

        ref var tanksSearchSpace = ref MemoryMarshal.GetReference(tanks);
        for (var i = 0; i < tanks.Length; i++) {
            var tnk = Unsafe.Add(ref tanksSearchSpace, i);
            if (tnk is not null && !tnk.Dead) cnt++;
        }

        return cnt;
    }
    public static int GetTankCountOfType(int tier) {
        var cnt = 0;
        Span<AITank> tanks = GameHandler.AllAITanks;

        ref var tanksSearchSpace = ref MemoryMarshal.GetReference(tanks);
        for (var i = 0; i < tanks.Length; i++) {
            var tnk = Unsafe.Add(ref tanksSearchSpace, i);
            if (tnk is not null && tnk.AiTankType == tier && !tnk.Dead) cnt++;
        }

        return cnt;
    }
}