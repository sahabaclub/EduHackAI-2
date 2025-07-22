import React, { useEffect, useState } from "react";
import { Gantt, Task, ViewMode } from "gantt-task-react";
import "gantt-task-react/dist/index.css";

const GanttApp = () => {
  const [tasks, setTasks] = useState([]);
  const [view, setView] = useState(ViewMode.Day);

  useEffect(() => {
    fetch("/api/ganttdata")
      .then((res) => res.json())
      .then((data) => setTasks(data));
  }, []);

  return (
    <div>
      <h2>Project Gantt Chart</h2>
      <div style={{ background: "#fff", borderRadius: 8, padding: 16 }}>
        <Gantt tasks={tasks} viewMode={view} listCellWidth={"155px"} />
      </div>
      <div style={{ marginTop: 16 }}>
        <button onClick={() => setView(ViewMode.Day)}>Day</button>
        <button onClick={() => setView(ViewMode.Week)}>Week</button>
        <button onClick={() => setView(ViewMode.Month)}>Month</button>
      </div>
    </div>
  );
};

export default GanttApp;

// Mount to window for non-React host
window.renderGanttApp = function (containerId, tasks) {
  const root = document.getElementById(containerId);
  if (root) {
    ReactDOM.render(<GanttApp />, root);
  }
};
