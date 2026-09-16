using Xunit;

public static class Calculator
{
    public static int Add(int left, int right) => left + right;
}

public sealed class CalculatorTests
{
    [Fact]
    public void Add_returns_the_sum_of_two_numbers()
    {
        var result = Calculator.Add(2, 3);

        Assert.Equal(5, result);
    }
}
