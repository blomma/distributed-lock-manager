using System;

namespace RLock
{
    class Program {
        static void Main(string[] args) {
            var s = new RedisDistributedLockManager("localhost");
            var result = s.Lock("test", new TimeSpan(0, 40, 0));
            if (!result.success) {
                return;
            }

            // Do BIZ


            // Unlock
            s.Unlock(result.redisDistributedLock);
        }
    }
}
