method SwapArithmetic(X: int, Y: int) returns(x: int, y: int)
  ensures x==Y
  ensures y==X

{
  x, y := X, Y;

  x := x - y;//buggy line

  y := y - x;
  x := y + x;

}
