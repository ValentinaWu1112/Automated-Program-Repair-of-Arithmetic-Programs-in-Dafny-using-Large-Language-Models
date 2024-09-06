method nonZeroReturn(x: int) returns (y: int)
  ensures y != 0
{
  if x == 0 {
    return x + 0;
  } else {
    return -x;
  }
}
method test() {
  var input := nonZeroReturn(-1);
}
