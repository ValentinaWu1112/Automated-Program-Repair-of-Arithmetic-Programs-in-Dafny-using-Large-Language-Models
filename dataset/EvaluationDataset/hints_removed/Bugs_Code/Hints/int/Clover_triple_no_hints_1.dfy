method Triple (x:int) returns (r:int)
  ensures r==3*x
{
  r := x * -1;//buggy line

}
