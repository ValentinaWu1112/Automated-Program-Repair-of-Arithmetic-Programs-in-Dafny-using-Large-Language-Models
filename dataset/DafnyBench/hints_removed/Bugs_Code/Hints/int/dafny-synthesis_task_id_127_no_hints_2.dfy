method Multiply(a: int, b: int) returns (result: int)
  ensures result == a * b
{
  result := b * b;//buggy line

}
