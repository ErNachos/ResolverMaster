using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ResolverMaster.Editor.Validation
{
    /// <summary>
    /// A validation check. Every non-abstract class implementing it (with a parameterless constructor) is run by <see cref="Validator"/>:
    /// a minigame adds its own checks (levels solvable, art present...) just by declaring a new class, nothing to register.
    /// </summary>
    public interface IValidationCheck
    {
        string Name { get; }
        void Run(ValidationReport report);
    }

    public sealed class ValidationReport
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public string Current;

        public void Error(string message) => Errors.Add($"[{Current}] {message}");
        public void Warning(string message) => Warnings.Add($"[{Current}] {message}");
        public bool Ok => Errors.Count == 0;
    }

    /// <summary>Menu Resolver Master > Validate. Batch: -executeMethod ResolverMaster.Editor.Validation.Validator.RunBatch (exit 0 OK, 3 errors).</summary>
    public static class Validator
    {
        [MenuItem("Resolver Master/Validate")]
        public static ValidationReport RunMenu() => Run(log: true);

        public static void RunBatch()
        {
            var report = Run(log: true);
            EditorApplication.Exit(report.Ok ? 0 : 3);
        }

        public static IEnumerable<IValidationCheck> Checks() =>
            TypeCache.GetTypesDerivedFrom<IValidationCheck>()
                .Where(t => !t.IsAbstract && !t.IsInterface && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.FullName)
                .Select(t => (IValidationCheck)Activator.CreateInstance(t));

        public static ValidationReport Run(bool log)
        {
            var report = new ValidationReport();
            var names = new List<string>();
            foreach (var check in Checks())
            {
                report.Current = check.Name;
                names.Add(check.Name);
                try { check.Run(report); }
                catch (Exception e) { report.Error("check crashed: " + e); }
            }
            if (log)
            {
                var sb = new StringBuilder();
                sb.Append($"Validator: {(report.Ok ? "OK" : "FAILED")} - {names.Count} checks ({string.Join(", ", names)}), {report.Errors.Count} errors, {report.Warnings.Count} warnings");
                foreach (var e in report.Errors) sb.Append("\n  ERROR ").Append(e);
                foreach (var w in report.Warnings) sb.Append("\n  WARNING ").Append(w);
                if (report.Ok) Debug.Log(sb.ToString());
                else Debug.LogError(sb.ToString());
            }
            return report;
        }
    }
}
