using System;
using StackExchange.Redis;

public class RedisDistributedLock
{
    public RedisKey Key { get; private set; }

    public RedisValue Value { get; private set; }

    public TimeSpan Validity { get; private set; }

    public RedisDistributedLock(RedisKey key, RedisValue value, TimeSpan validity)
    {
        Key = key;
        Value = value;
        Validity = validity;
    }
}

public class RedisDistributedLockManager
{
    private readonly ConnectionMultiplexer redis;

    public RedisDistributedLockManager(string connectionString)
    {
        try
        {
            var options = ConfigurationOptions.Parse(connectionString);
            redis = ConnectionMultiplexer.Connect(options.ToString());
        }
        catch (Exception ex)
        {
            // logger.Error(ex, "Failed to connect to Redis");
        }
    }

    private bool IsConnected()
    {
        if (redis == null)
        {
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

    private byte[] CreateUniqueLockId()
    {
        return Guid.NewGuid().ToByteArray();
    }

    public (bool success, RedisDistributedLock redisDistributedLock) Lock(string key, TimeSpan ttl)
    {
        if (!IsConnected())
        {
            return (success: false, redisDistributedLock: null);
        }

        var value = CreateUniqueLockId();
        var result = redis.GetDatabase().StringSet(key, value, ttl, When.NotExists);
        if (!result)
        {
            return (success: false, redisDistributedLock: null);
        }

        return (success: true, redisDistributedLock: new RedisDistributedLock(key, value, ttl));
    }

    public void Unlock(RedisDistributedLock redisDistributedLock)
    {
        RedisKey[] key = { redisDistributedLock.Key };
        RedisValue[] values = { redisDistributedLock.Value };
        var result = redis.GetDatabase().ScriptEvaluate(
            UnlockScript,
            key,
            values
        );
    }
}