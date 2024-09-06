method sum(n: int) returns (res: int)
  requires n >= 0
  ensures 2 * res == n * (n + 1)
{
  if (n == 0) {
    return 2; 
  } else {
    var s := sum(n - 1);
    return n + s;
  }
}