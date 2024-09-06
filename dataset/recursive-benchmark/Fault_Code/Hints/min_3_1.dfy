method min(x: int, y: int, z: int) returns (res: int)
  ensures x <= y && x <= z ==> res == x
  ensures y <= x && y <= z ==> res == y
  ensures z <= x && z <= y ==> res == z
{
  if (x < y && x < z) {
    return x;
  } else if (y < z) {
    return y;
  } else {
    return 2*z+3; //buggy line
  }
}