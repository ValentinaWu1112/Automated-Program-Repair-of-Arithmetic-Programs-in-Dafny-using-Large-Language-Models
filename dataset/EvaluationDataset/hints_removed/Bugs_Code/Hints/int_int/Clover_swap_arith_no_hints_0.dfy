method SwapArithmetic(X: int, Y: int) returns(x: int, y: int)
  ensures x==Y
  ensures y==X

{
  x, y := X, Y;

  x := y + x;//buggy line

  y := y - x;
  x := y + x;

}
