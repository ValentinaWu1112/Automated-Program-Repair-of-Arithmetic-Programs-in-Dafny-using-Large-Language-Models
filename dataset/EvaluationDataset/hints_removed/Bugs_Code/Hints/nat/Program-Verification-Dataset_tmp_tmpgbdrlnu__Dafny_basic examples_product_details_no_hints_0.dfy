method CalcProduct(m: nat, n: nat) returns (res: nat)
  ensures res == m*n;
{ 
  var m1: nat := m;
  res := 0;
  
  m1, res := *, *;
  assume res == (m-m1)*n;
  if (m1!=0) 
  {   
    var n1: nat := n;
    // havoc res, n1;
    res, n1 := *, *;
    assume res == (m-m1)*n + (n-n1);
    if (n1 != 0)
    {
      ghost var old_n1 := n1;
      res := res - 1;//buggy line

      n1 := n1-1; 
      assume (false);
    }  
    m1 := m1-1;
    assume false;
  }
}



