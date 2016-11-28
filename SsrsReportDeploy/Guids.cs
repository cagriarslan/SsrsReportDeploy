// Guids.cs
// MUST match guids.h

using System;

namespace SsrsReportDeploy
{
    static class GuidList
    {
        public const string guidSsrsReportDeployCmdSetCodeString = "1BD50177-0D74-48C3-AEC5-082C75061784";
        public const string guidSsrsReportDeployCmdSetItemString = "E5D6DFF3-53AD-41B5-BC72-9818E4FA2ABC";
        public const string guidSsrsReportDeployCmdSetProjectString = "8906522C-7AD8-42D0-9A4F-8E678AAE9ACB";
        public const string guidSsrsReportDeployCmdSetSolutionString = "b6e4a7c4-42be-40d2-a037-49dd893c29a4";
        public const string guidSsrsReportDeployPkgString = "4fe11457-e2e7-4a77-be51-1fcdcc6e8fb0";
        public const string guidAutoT4MVCPkgString = "c676817c-46cc-47d3-b03c-8a05f499d4a5";
        public const string guidAutoT4MVCCmdSetString = "3e7abe3a-4955-4c2f-aef7-4672394b69fd";

        public static readonly Guid guidAutoT4MVCCmdSet = new Guid(guidAutoT4MVCCmdSetString);
        public static readonly Guid guidSsrsReportDeployCmdSetCode = new Guid(guidSsrsReportDeployCmdSetCodeString);
        public static readonly Guid guidSsrsReportDeployCmdSetItem = new Guid(guidSsrsReportDeployCmdSetItemString);
        public static readonly Guid guidSsrsReportDeployCmdSetProject = new Guid(guidSsrsReportDeployCmdSetProjectString);
        public static readonly Guid guidSsrsReportDeployCmdSetSolution = new Guid(guidSsrsReportDeployCmdSetSolutionString);
    }
}