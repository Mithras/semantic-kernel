using System.Globalization;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace Microsoft.SemanticKernelTest.Skills;

// Copy of Microsoft.SemanticKernel.CoreSkills.MathSkill
public class NativeMath
{
    [SKFunction("Adds value to a value")]
    [SKFunctionName("Add")]
    [SKFunctionInput(Description = "The value to add")]
    [SKFunctionContextParameter(Name = "Amount", Description = "Amount to add")]
    public Task<string> AddAsync(string input, SKContext context) =>
        AddOrSubtractAsync(input, context, add: true);

    [SKFunction("Subtracts value to a value")]
    [SKFunctionName("Subtract")]
    [SKFunctionInput(Description = "The value to subtract")]
    [SKFunctionContextParameter(Name = "Amount", Description = "Amount to subtract")]
    public Task<string> SubtractAsync(string input, SKContext context) =>
        AddOrSubtractAsync(input, context, add: false);

    private static Task<string> AddOrSubtractAsync(string initialValueText, SKContext context, bool add)
    {
        if (!int.TryParse(initialValueText, NumberStyles.Any, CultureInfo.InvariantCulture, out var initialValue))
        {
            return Task.FromException<string>(new ArgumentOutOfRangeException(
                nameof(initialValueText), initialValueText, "Initial value provided is not in numeric format"));
        }

        string contextAmount = context["Amount"];
        if (!int.TryParse(contextAmount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            return Task.FromException<string>(new ArgumentOutOfRangeException(
                nameof(context), contextAmount, "Context amount provided is not in numeric format"));
        }

        var result = add
            ? initialValue + amount
            : initialValue - amount;

        return Task.FromResult(result.ToString(CultureInfo.InvariantCulture));
    }
}
