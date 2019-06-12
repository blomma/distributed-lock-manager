using System;

namespace RedisDistributedLockManager {
    class Program {
        static void Main(string[] args) {
            using (var redisDistributedLockManager = new RedisDistributedLockManager("localhost")) {
                var success = redisDistributedLockManager.Lock("test", new TimeSpan(0, 40, 0));
                if (!success) {
                    return;
                }
            }
        }
    }
}
