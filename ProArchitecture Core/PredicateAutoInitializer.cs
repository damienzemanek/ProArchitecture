using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using ProArchitecture.Predicates;

namespace ProArchitecture.Predicates
{
    public static class PredicateAutoInitializer
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RefreshAllPredicates()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                string name = assembly.FullName;
                // Optimization: Skip system/Unity assemblies
                if (name.StartsWith("System") || 
                    name.StartsWith("Unity") || 
                    name.StartsWith("mscorlib") || 
                    name.StartsWith("Mono.") || 
                    name.StartsWith("nunit."))
                    continue;

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        // We only care about static classes (abstract + sealed in C#)
                        if (!type.IsAbstract || !type.IsSealed) continue;

                        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        foreach (var field in fields)
                        {
                            if (field.FieldType == typeof(Predicate))
                            {
                                // Accessing the value forces the static initializer to run,
                                // which remaps the function pointer (&method) to the new domain.
                                _ = field.GetValue(null);
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Some assemblies might fail to load types, skip them
                }
            }
        }
    }
}
