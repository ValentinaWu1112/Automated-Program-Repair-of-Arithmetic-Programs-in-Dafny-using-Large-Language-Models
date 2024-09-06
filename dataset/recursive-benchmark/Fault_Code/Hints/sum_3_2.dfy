method sum(x: int, y: int, z:int) returns (res: int)
  requires true
  ensures res == x + y + y
{
  return 2*x + y + z; //buggy line
}