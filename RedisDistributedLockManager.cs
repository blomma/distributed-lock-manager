namespace RedisDistributedLockManager {
    using System;
    using StackExchange.Redis;

    public class RedisDistributedLock {
        public RedisKey Key { get; private set; }

        public RedisValue Value { get; private set; }

        public TimeSpan Validity { get; private set; }

        public RedisDistributedLock(RedisKey key, RedisValue value, TimeSpan validity) {
            Key = key;
            Value = value;
            Validity = validity;
        }
    }

    public class RedisDistributedLockManager : IDisposable {
        private readonly ConnectionMultiplexer connectionMultiplexer;
        private RedisDistributedLock redisDistributedLock;

        public RedisDistributedLockManager(string connectionString) {
            var options = ConfigurationOptions.Parse(connectionString);
            connectionMultiplexer = ConnectionMultiplexer.Connect(options.ToString());
        }

        public RedisDistributedLockManager(ConnectionMultiplexer connectionMultiplexer) {
            this.connectionMultiplexer = connectionMultiplexer;
        }

        private bool IsConnected() {
            if (connectionMultiplexer == null) {
                return false;
            }

            return connectionMultiplexer.IsConnected;
        }

        private const String UnlockScript = @"
                if redis.call(""get"",KEYS[1]) == ARGV[1] then
                    return redis.call(""del"",KEYS[1])
                else
                    return 0
                end";

        private byte[] CreateUniqueLockId() {
            return Guid.NewGuid().ToByteArray();
        }

        public bool Lock(string key, TimeSpan ttl) {
            if (!IsConnected()) {
                return false;
            }

            var value = CreateUniqueLockId();
            var result = connectionMultiplexer.GetDatabase().StringSet(key, value, ttl, When.NotExists);
            if (!result) {
                return false;
            }

            redisDistributedLock = new RedisDistributedLock(key, value, ttl);

            return true;
        }

        public void Unlock() {
            if (redisDistributedLock == null) {
                return;
            }

            RedisKey[] key = { redisDistributedLock.Key };
            RedisValue[] values = { redisDistributedLock.Value };
            var result = connectionMultiplexer.GetDatabase().ScriptEvaluate(
                UnlockScript,
                key,
                values
            );
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    Unlock();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}