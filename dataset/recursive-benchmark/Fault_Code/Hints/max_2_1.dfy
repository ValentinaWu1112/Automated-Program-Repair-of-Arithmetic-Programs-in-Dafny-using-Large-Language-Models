method max(x: int, y: int) returns (res: int)
  ensures x >= y ==> res == x 
  ensures y >= x ==> res == y
{
  if (x > y) {
    return x + 2; //buggy line
  } else {
    return y;
  }
}