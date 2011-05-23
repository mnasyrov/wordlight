// Guids.cs
// MUST match guids.h
using System;

namespace MikhailNasyrov.WordLightPackage
{
    static class GuidList
    {
        public const string guidWordLightPackagePkgString = "22cd2dc2-d883-46bc-9304-c43a1aaf9c5f";
        public const string guidWordLightPackageCmdSetString = "6c689197-b42e-4045-8230-9e09996146bd";

        public static readonly Guid guidWordLightPackageCmdSet = new Guid(guidWordLightPackageCmdSetString);
    };
}