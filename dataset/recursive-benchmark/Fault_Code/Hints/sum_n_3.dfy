method sum(n: int) returns (res: int)
  requires n >= 0
  ensures 2 * res == n * (n + 1)
{
  if (n == 0) {
    return 0;
  } else {
    var k := sum(n - 1);
    return n - k; //buggy line
  }
}