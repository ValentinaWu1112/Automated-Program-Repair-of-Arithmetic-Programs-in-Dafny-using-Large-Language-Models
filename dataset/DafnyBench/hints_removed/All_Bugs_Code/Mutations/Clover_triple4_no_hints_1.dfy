method Triple (x:int) returns (r:int)
  ensures r==3*x
{
  var y := x * -1;
  r := y+x;
}
