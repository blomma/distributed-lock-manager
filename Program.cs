using System;

namespace RLock
{
    class Program
    {
        static void Main(string[] args)
        {
            var s = new RedisDistributedLockManager("");
            var result = s.Lock("", new TimeSpan(0, 40, 0));
            if (!result.success)
            {
                return;
            }

            // Do BIZ


            // Unlock
            s.Unlock(result.redisDistributedLock);
        }
    }
}
