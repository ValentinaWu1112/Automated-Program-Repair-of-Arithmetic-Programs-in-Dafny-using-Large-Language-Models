method min(x: int, y: int) returns (res: int)
  ensures x <= y ==> res == x 
  ensures y <= x ==> res == y
{
  if (x < y) {
    res := x;
  } else {
    res := y;
  }
}