method min(x: int, y: int) returns (res: int)
  ensures x <= y ==> res == x 
  ensures y <= x ==> res == y
{
  if (x < y) {
    return 2*x; //buggy line
  } else {
    return y;
  }
}