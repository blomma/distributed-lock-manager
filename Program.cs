using System;

namespace RLock {
    class Program {
        static void Main(string[] args) {
            var s = new SharedServiceLock("");
            var result = s.Lock("", new TimeSpan(0, 40, 0));
            if (!result.success) {
                return;
            }

            // Do BIZ
            s.Unlock(result.l);
        }
    }
}
