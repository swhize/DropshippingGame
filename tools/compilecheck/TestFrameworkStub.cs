// Nachbildung der benötigten Typen aus com.unity.test-framework (UnityEngine.TestRunner) für den
// Compile-Check der PlayMode-Tests ohne Unity. NUnit selbst kommt aus dem NuGet-Cache (NUnit 3.14).
// Nur die Signaturen, die Tests/PlayMode benutzt – echte Prüfung macht Unity.
using System;

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Method)]
    public class UnityTestAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class UnitySetUpAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class UnityTearDownAttribute : Attribute
    {
    }

    public static class LogAssert
    {
        public static bool ignoreFailingMessages { get; set; }

        public static void Expect(LogType type, string message)
        {
        }

        public static void NoUnexpectedReceived()
        {
        }
    }
}
