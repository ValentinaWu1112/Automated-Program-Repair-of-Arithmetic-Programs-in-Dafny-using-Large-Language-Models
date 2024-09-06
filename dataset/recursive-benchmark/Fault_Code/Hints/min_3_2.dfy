method min(x: int, y: int, z: int) returns (res: int)
  ensures x <= y && x <= z ==> res == x
  ensures y <= x && y <= z ==> res == y
  ensures z <= x && z <= y ==> res == z
{
  if (2*x < y && x < z) { //buggy line
    return x;
  } else if (y < z) {
    return y;
  } else {
    return z;
  }
}