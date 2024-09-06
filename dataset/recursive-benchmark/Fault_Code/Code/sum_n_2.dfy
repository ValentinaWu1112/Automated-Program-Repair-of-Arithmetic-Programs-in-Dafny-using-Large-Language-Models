method sum(n: int) returns (res: int)
  requires n >= 0
  ensures res == n * (n + 1)/2
{
  if (n == 0) {
    return 0;
  } else {
    var s := sum(n - 1);
    return 2*n + 3 + s; 
  }
}