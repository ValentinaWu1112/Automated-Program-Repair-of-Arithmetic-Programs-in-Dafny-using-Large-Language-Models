method sum(x: int, y: int, z:int) returns (res: int)
  requires true
  ensures res == x + y + y
{
  return x - y + 2 * z; 
}