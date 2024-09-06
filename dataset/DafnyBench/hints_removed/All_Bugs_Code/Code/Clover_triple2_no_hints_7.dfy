method Triple (x:int) returns (r:int)
  ensures r==3*x
{
  if {
    case x<18 =>
      var a, b := 2 * x, 3 / x;
      r:=(a+b)/2;
    case 0<=x =>
      var y:=2*x;
      r:= x+y;
  }
}
