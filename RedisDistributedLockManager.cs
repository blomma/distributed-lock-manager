using System;
using StackExchange.Redis;

public class Lock {

    public RedisKey Key { get; private set; }

    public RedisValue Value { get; private set; }

    public TimeSpan Validity { get; private set; }

    public Lock(RedisKey key, RedisValue value, TimeSpan validity) {
        Key = key;
        Value = value;
        Validity = validity;
    }
}

public class SharedServiceLock {
    private readonly ConnectionMultiplexer redis;

    public SharedServiceLock(string connectionString) {
        try {
            var options = ConfigurationOptions.Parse(connectionString);
            redis = ConnectionMultiplexer.Connect(options.ToString());
        } catch (Exception) {
            // logger.Error(ex, "Failed to connect to Redis");
        }
    }

    private bool IsConnected() {
        if (redis == null) {
            return false;
        }

        return redis.IsConnected;
    }

    const String UnlockScript = @"
            if redis.call(""get"",KEYS[1]) == ARGV[1] then
                return redis.call(""del"",KEYS[1])
            else
                return 0
            end";

    private byte[] CreateUniqueLockId() {
        return Guid.NewGuid().ToByteArray();
    }

    public (bool success, Lock l) Lock(string key, TimeSpan ttl) {
        if (!IsConnected()) {
            return (success: false, l: null);
        }

        var value = CreateUniqueLockId();
        var result = redis.GetDatabase().StringSet(key, value, ttl, When.NotExists);
        if (!result) {
            return (success: false, l: null);
        }

        return (success: true, l: new Lock(key, value, ttl));
    }

    public void Unlock(Lock l) {
        RedisKey[] key = { l.Key };
        RedisValue[] values = { l.Value };
        var result = redis.GetDatabase().ScriptEvaluate(
            UnlockScript,
            key,
            values
        );
    }
}