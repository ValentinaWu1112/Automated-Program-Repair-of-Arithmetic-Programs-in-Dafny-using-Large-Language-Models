method nonZeroReturn(x: int) returns (y: int)
  ensures y != 0
{
  if x == 0 {
    return x - 1;//buggy line

  } else {
    return -x;
  }
}
method test() {
  var input := nonZeroReturn(-1);
}
