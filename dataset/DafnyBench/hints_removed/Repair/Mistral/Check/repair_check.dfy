method abs(x: int) returns (res: int)
  ensures x >= 0 ==> res == x
  ensures x < 0 ==> res == -x
{
  if (x >= 0) { 
    return x;
  } else {
 return -x;
  }
}