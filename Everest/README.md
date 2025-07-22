# App01
Scrum Master Application project

Project features
----------------
1. Application allows the project planning for a scrum master to distribute task to the teammates
2. Key features:
    - used Open AI model to create task list for the teammates
    - Gantt chart with task list and option to edit the timeline of the task. Upon changes the supsequenct task will out arrange in the project timeline. For eg: if the second task's duration is change from 2 days to 5 days then the end for the second task will be udpated as well all the tasks that depends on this task will change its start date based on previous task's end date.
    - Enabled Form Authentication for the users to login to the application
    - They can view their task from the dashboard
    - Security features to are followed to allow only the authorized users to access the application. Users with role SuperAdmin or Scrum master can only access the Gantt chart or task list creation page. Other Type of users cannot access this pages.


Demo Video Link : https://youtu.be/8gBN7fSZMP8