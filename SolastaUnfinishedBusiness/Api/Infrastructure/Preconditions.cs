﻿using System;
using System.Diagnostics;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.Diagnostics;

namespace SolastaUnfinishedBusiness.Api.Infrastructure;

public static class Preconditions
{
    [ContractAnnotation("halt <= paramValue : null")]
    [AssertionMethod]
    public static void ArgumentIsNotNull<T>([NotNull] [NoEnumeration] T paramValue,
        [InvokerParameterName] string paramName)
        where T : class
    {
        if (paramValue == null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    [ContractAnnotation("halt <= paramValue : null")]
    [AssertionMethod]
    public static void IsNotNullOrEmpty([NotNull] string paramValue, [InvokerParameterName] string paramName)
    {
        if (string.IsNullOrEmpty(paramValue))
        {
            throw new ArgumentException(@"The parameter must not be null or an empty string.", paramName);
        }
    }

    [ContractAnnotation("halt <= paramValue : null")]
    [AssertionMethod]
    public static void IsNotNullOrWhiteSpace([NotNull] string paramValue, [InvokerParameterName] string paramName)
    {
        if (string.IsNullOrWhiteSpace(paramValue))
        {
            throw new ArgumentException(@"The parameter must not be null or whitespace.", paramName);
        }
    }

    [Conditional("DEBUG")]
    [AssertionMethod]
    public static void IsValidDuration(RuleDefinitions.DurationType type, int duration)
    {
        if (RuleDefinitions.IsVariableDuration(type))
        {
            if (duration == 0)
            {
                throw new ArgumentException($@"A duration value is required for duration type {type}.",
                    nameof(duration));
            }
        }
        else
        {
            if (duration != 0)
            {
                throw new ArgumentException(
                    $@"Duration={duration}. A duration value is not expected for duration type {type}.",
                    nameof(duration));
            }
        }
    }

    [Conditional("DEBUG")]
    [AssertionMethod]
    internal static void AreEqual<T>([NotNull] T left, T right, string message)
    {
        if (!left.Equals(right))
        {
            throw new SolastaUnfinishedBusinessException(message);
        }
    }
}
