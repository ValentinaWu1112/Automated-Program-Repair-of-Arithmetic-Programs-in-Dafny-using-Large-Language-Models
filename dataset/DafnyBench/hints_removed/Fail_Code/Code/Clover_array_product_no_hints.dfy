method arrayProduct(a: array<int>, b: array<int>) returns (c: array<int> )
  requires a.Length==b.Length
  ensures c.Length==a.Length
  ensures forall i:: 0 <= i< a.Length==> a[i] * b[i]==c[i]
{
  c:= new int[a.Length];
  var i:=0;
  while i<a.Length
  {
    c[i]:=a[i]*b[i];
    i:=i+1;
  }
}
