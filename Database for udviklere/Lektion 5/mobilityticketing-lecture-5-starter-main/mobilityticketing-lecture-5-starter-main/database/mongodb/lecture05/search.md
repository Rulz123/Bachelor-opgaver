MongoDB array query bug
Problem

The original query used two independent conditions on the departures array:

departures.status had to equal "Scheduled".
departures.departureUtc had to fall within the requested time interval.

MongoDB could satisfy these conditions using two different elements of the same array. In document B, one departure satisfied the status condition, while another satisfied the time condition. Consequently, the document matched even though no single departure satisfied both requirements.

Fix

I replaced the independent array-field conditions with $elemMatch:

javascript
departures: {
  $elemMatch: {
    status: "Scheduled",
    departureUtc: {
      $gte: start,
      $lt: end
    }
  }
}


$elemMatch requires at least one departure element to satisfy both conditions simultaneously. After this change, document B no longer matches the query.

Result

The original query produced a false positive because its conditions could match different array elements. The corrected query matches only documents containing a scheduled departure within the requested time interval.