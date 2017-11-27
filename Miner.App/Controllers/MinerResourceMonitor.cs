﻿using System;
using System.Threading;

namespace HD
{
  public class MinerResourceMonitor
  {
    readonly MiddlewareServer server;
    Thread thread;
    long sleepForInNanoseconds = 206892080;
    long deltaSleepForLastFrame;
    int countSameDirection;

    public MinerResourceMonitor(
      MiddlewareServer server)
    {
      this.server = server;
    }

    public void Start()
    {
      UpdateSleepFor();
      thread = new Thread(Run);
      thread.Start();
    }

    public void Stop()
    {
      thread.Abort();
    }

    void Run()
    {
      while (true)
      {
        if (HardwareMonitor.percentTotalCPU - HardwareMonitor.percentMinerCPU > Miner.instance.currentTargetCpu)
        { // Something else is using the entire budget
          Miner.instance.Stop();
          return;
        }
        UpdateSleepFor();

        Thread.Sleep(100);
      }
    }

    void UpdateSleepFor()
    {
      // Possible range is (-1, 1)
      double deltaTargetCpu = HardwareMonitor.percentTotalCPU - Miner.instance.currentTargetCpu;

      if(Math.Abs(deltaTargetCpu) < .025)
      { // Close enough
        countSameDirection = 0;
        deltaSleepForLastFrame = 0;
        return;
      }

      countSameDirection++;
      long delta = (long)(100000 * deltaTargetCpu * countSameDirection);

      if (Math.Sign(deltaTargetCpu) != Math.Sign(deltaSleepForLastFrame) && Math.Sign(deltaSleepForLastFrame) != 0)
      { // Over-corrected, slow down
        deltaSleepForLastFrame = 0;
        countSameDirection = 0;
      }
      else
      {
        deltaSleepForLastFrame = delta;
      }

      if (delta != 0)
      {
        sleepForInNanoseconds += delta;
        if (sleepForInNanoseconds < 0)
        {
          sleepForInNanoseconds = 0;
        }
        Console.WriteLine(sleepForInNanoseconds);
        server.Send(new SetSleepFor(sleepForInNanoseconds));
      }
    }
  }
}
