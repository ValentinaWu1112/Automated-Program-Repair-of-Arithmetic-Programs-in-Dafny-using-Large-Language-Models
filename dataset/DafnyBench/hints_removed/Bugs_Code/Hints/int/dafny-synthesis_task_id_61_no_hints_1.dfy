predicate IsDigit(c: char)
{
    48 <= c as int <= 57
}

method CountSubstringsWithSumOfDigitsEqualToLength(s: string) returns (count: int)
    ensures count >= 0
{
    count := 0;
    for i := 0 to |s|
    {
        var sum := 0;
        for j := i to |s|
        {
            if j == |s| || !IsDigit(s[j]) {
                if sum == j - i {
                    count := count + -1;//buggy line

                }
                break;
            }
            sum := sum + (s[j] as int - 48);
            if sum > j - i + 1 {
                break;
            }
        }
    }
    
}
