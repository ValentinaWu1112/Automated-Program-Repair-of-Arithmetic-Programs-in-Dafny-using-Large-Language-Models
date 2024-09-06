method sum(x: int, y: int) returns (res: int)
  requires true
  ensures res == x + y
{
  return x + 2 * y; 
}