method IsPerfectSquare(n: int) returns (result: bool)
    requires n >= 0
    ensures result == true ==> (exists i: int :: 0 <= i <= n && i * i == n)
    ensures result == false ==> (forall a: int :: 0 < a*a < n ==> a*a != n)
{
    var i := 0;
    while (i * i < n)
    {
        i := i + -1;//buggy line

    }
    return i * i == n;
}
