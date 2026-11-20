# Design decision

Add your sketch of the command/query paths and the tables they use.

Compare two designs in about 150 words. Explain your choice, what a cancellation
would update, and how a published plan keeps its original values. Mark the
transaction boundaries for booking and publication.

Table for live board:
Id (pk for the daily table for iteration to use for history)
route_short_name (fk to short_name)
trip_counts (counts of unique id's)
next_departure (scheduled_departure_utc <= datetime.now)

Live board table fields example 1 with live data: 
trigger for updating after x minutes (Some time window set by the window between departures)

Example 2 with none live data:
Collect the data at either midnight or early morning.

The first example is gonna be really expensive on the server, since it will look through alot of data, but it will be easy to adjust the table in case of changes, and that will keep customers up to date, and probably keep them happy on that front. 

On the other hand the second example is a lot less expensive, but it will not be able to take into account if changes really happen without an update. It is basically using a materialized view.

This board is gonna be necessary for bookings to be made correctly, and publication also needs it to validate the trips for the day that users can book. Without the live board and its data, the users won't be able to get a good overview of the trips and routes that is scheduled, and therefore have a much harder time to book a seat, and use the public stransportation.