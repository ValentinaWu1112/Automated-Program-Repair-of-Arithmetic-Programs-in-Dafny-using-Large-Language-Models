method SwapArithmetic(X: int, Y: int) returns(x: int, y: int)
  ensures x==Y
  ensures y==X

{
  x, y := X, Y;

  x := y - x;
  y := y - y;//buggy line

  x := y + x;

}
