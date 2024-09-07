method Abs(x: int) returns (y: int)
	requires x == -1
	ensures 0 <= y
	ensures 0 <= x ==> y == x
	ensures x < 0 ==> y == -x
{
	return x + 2;
}

method Abs2(x: real) returns (y: real)
	requires x == -0.5
	ensures 0.0 <= y
	ensures 0.0 <= x ==> y == x
	ensures x < 0.0 ==> y == -x
{
	return x - 1.0;//buggy line

}

method Main()
{
	var a := Abs(-1);
	var a2 := Abs2(-0.5);
}

