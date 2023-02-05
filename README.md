# Timer

Timer logs and calculates the time spent on a story based on your logging. This version is designed for programmers.
<br>
Download the executable: https://drive.google.com/drive/folders/1jRGDC-J8TB_M-BRELUQnkWROjGm3s4oI?usp=sharing
<br><br>
![Screenshot](https://github.com/AndreiVaida/Timer/blob/Programming/Resources/Screenshot%2023-02-05%134158.png?raw=true "Screenshot")
## Instructions
1. Input the activity name. It can be the Jira ID.
   1. Note: At startup, Timer will load your latest activity, and you can skip Step 2.
2. Click on the "Crează" button. A csv file with that name will be created in the "Activities" folder.
   1. Note: If an activity with the same name already exists, it will be loaded.
3. If your activity was loaded, the state of the window is updated as well (in the state in which you left the activity previously).
4. Click on the 8 buttons to start a "step" of your work:
   1. "**👨‍🎓 Ceremonie**" - Attend a Scrum Ceremony or any other meeting that is not focused on your activity.
   2. "**🤔 Investighez**" - Investigate/troubleshoot the solution/problem (alone or with others).
   3. "**✏ Implementez**" - Implement the task/fix (coding).
   4. "**⏳ Aștept Review**" - Waiting for your commit to be reviewed. <br>_Note: This is a parallel step, so you can do other "steps" meanwhile, for example reviewing others' commits._
   5. "**✏ Rezolv comentarii**" - Resolve the comments received for your commit. <br>_Note: The "⏳ Aștept Review" step is automatically stopped._
   6. "**✏ Fac Review**" - You do code review to other teammates.
   7. "**⏳ Aștept procesare**" - Wait for a build to execute, a file to download etc.  <br>_Note: This is a parallel step, so you can do other "steps" meanwhile._
   8. "**⏸️ Pauză**" - Take a break or finish the working day.
   <br>
   You can click the buttons in any order and as many times you want.
5. Clicking a button represents the start time of the step, respectively the end time of an active parallel step. The date-time (seconds precision) is recorded in the csv file. You can edit the file if you need to change the logs.
6. While the step is active, that button will look like it's pressed.
7. The current duration of the active step and the total duration of all steps are calculated and displayed in real time under each button. If there are multiple active steps at the same time, the total time is increased as there was only one active step.
The duration is not saved in the file. Only the Pause step is not considered work.
8. As there are parallel tasks ("⏳ Aștept Review" and "⏳ Aștept procesare") you can have up to 3 active buttons at the same time.
9. Clicking a parallel button stops the sequential active step. Clicking a sequential or a parallel button does not stop a parallel button.
10. Clicking the "⏸️ Pauză" button stops all the other buttons.
##Benefits and an inconvenience:
- You will discover exactly how you spend your time in a working day.
- Makes you more responsible in managing your time.
- Forces you to click the buttons every time you start a new step.