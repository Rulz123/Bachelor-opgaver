Git hash:b4abfbd35bf0097eb6bfa2c4af431a082154c227
Repo link: https://github.com/Rulz123/Bachelor-opgaver/tree/Nichlas/Database%20for%20udviklere


How to find the work and run it:

Setup: Run the different compose.yaml files to initiate docker containers with necessary databases. There is 1 for each folder.



Lecture 1: To find the used queries use the folder database in lecture 1, and then look to the file 003_queries.sql.example.
The evidence is the picture inside of that folder too.
The ticketing_draft.sql has the markup for the database with the new constraints and primary keys.

Decisions as to why it is done this way: The check constraints does alot of work for us in this lecture. It is the thing that checks up on no negatives for example.  

A big decision in this lecture was also what should be considered the primary key, which we chose to just go with ID . It is already a identifier that was not related to another table, so the choice was rather obvious. The other choise was to make an entirely new identifier, but that just seemed a bit silly, when we already had a perfect ID field. 

The SQL in the 001_relational_baseline.sql is where the foreign keys are added for run time instead of via alter table. 



Lecture 2: Look for the queries and constraint creation in the init file of 010_ticketing_draft.sql and in migrations. 

The integrity map is in the docs folder.

We created some constraints for the invariants that was very important. We made it so that there could not be negatives in prices, and added other checks that could make invalid data.  

We went with alot of constraint work, and analyzed the results with our integrity map. We could not verify the tickets validity yet in this example or the vehicles that is used for the route, as there was no vehicle table. 



Lecture 3: All the evidence for the queries is in the pictues folder. There is steps from base to step 6.

The responsibility matrix is located in the docs folder. 

This exercise tries out 4 different ways of pulling the profit of each operator. What we found was that for high performance servers with alot of complex data, the function and trigger way works great with fully automation, but is very taxing on the performance. It is high in maintenance cost tho to run both functions and triggers, so the database is gonna be fairly complex but strong. 

The other option for low performance databases is materialised view. It would work great with a trigger that updates the view every time there is a update for example, but this also depends on how often the data is needed to be used. The cost of a materialised view is very low on the database, and is a great use choise for the weak databases.



Lecture 4: The queries to read and write for the different migrations is located in the experiments folder.

The queries for the migrations is in the migrations folder in the example files. 

There is a singular evidence piece in the evidence folder. 

For test just run the migration examples in order, and use the writes in the same order. First old reader/write, then new reader/writer for migration 1 and 2, then final reader/writer

If you run the migrations in order, you can see how easily it works with the different versions, where in the last version, you can just drop the product_code column in tickets, and it gives 0 problems. 

The problem with migrations is that if you don't test it properly, you will get dataloss from the failed migration, and that data is just gone usually. So before you migrate, it is always a good idea to do a backup, and have whatever message que hold on to the messages.

There is still some functionality that can be a bit wonky, as there is some seemingly incomplete tables in the database, and there is room for improvements in the functions and triggers, but overall it works pretty well.
The next that should be checked up is the setup off the vehicles capacity, and make sure that that is working, so we can't oversell seats for a trip. This is atleast the most glaring problem with mobility ticketing atm. 