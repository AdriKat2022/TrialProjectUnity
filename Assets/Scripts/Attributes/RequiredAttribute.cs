using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
public class RequiredAttribute : PropertyAttribute
{
    public readonly string WarningMessage = "This field is required";
    public readonly bool AllowEmptyString = false;
    public readonly WarningTypeEnum WarningType = WarningTypeEnum.Warning;

    public RequiredAttribute(string warningMessage, WarningTypeEnum warningType = WarningTypeEnum.Warning, bool allowEmptyString = false)
    {
        WarningMessage = warningMessage;
        WarningType = warningType;
        AllowEmptyString = allowEmptyString;
    }
    public RequiredAttribute()
    {
    }

    public enum WarningTypeEnum
    {
        Error = 3,
        Warning = 2,
        Info = 1
    }
}
