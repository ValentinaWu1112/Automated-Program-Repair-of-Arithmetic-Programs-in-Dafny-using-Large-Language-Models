method max(x: int, y: int) returns (res: int)
  ensures x >= y ==> res == x 
  ensures y >= x ==> res == y
{
  if (x > 2*y) { 
    return x;
  } else {
    return y;
  }
}