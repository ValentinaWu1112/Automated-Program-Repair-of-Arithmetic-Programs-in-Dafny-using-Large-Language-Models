method min(x: int, y: int) returns (res: int)
  ensures x <= y ==> res == x 
  ensures y <= x ==> res == y
{
  if (3*x < y) { 
    return x;
  } else {
    return y;
  }
}