using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIBN_Tools.KanbanizeService
{
    public class KanbanizeBoard
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Id { get; set; }
        public int Type { get; set; }           // 0: Kanban Board, 1: AI Canvas
        public bool IsArchived { get; set; }

        public List<KanbanizeWorkflow> Workflows { get; set; }
        public List<KanbanizeColumn> AllColumns { get; set; }



    }


    public class KanbanizeWorkflow
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public int Type { get; set; }           // 0: Cards, 1: Initiatives, 2: Timeline
        public int Position { get; set; }       // starting with position 0: topmost workflow
        public List<KanbanizeLane> Lanes { get; set; }
        public List<KanbanizeColumn> Columns { get; set; }


    }


    public class KanbanizeLane
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public int WorkflowId { get; set; }
        public int Position {  set; get; }      // Position within the workflow, 0 means first lane
        public List<KanbanizeCard> Cards { get; set; }


    }



    public class KanbanizeColumn
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int WorkflowId { get; set; }
        public int? ParentColumnId { get; set; }
        public int? Section { get; set; }
        public int Position { get; set; }
        public string Color { get; set; }
    }



    public class KanbanizeCard
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int Id { get; set; }
        public int BoardId { get; set; }
        public int WorkflowId { get; set; }
        public int LaneId { get; set; }
        public int ColumnId { get; set; }
        public int? Section {  get; set; }

        public double Size { get; set; }
        public DateTime Deadline { get; set; }
        public int IsBlocked { get; set; }
        public List<KanbanizeCardSubtask> Subtasks { get; set; }

        public string Color { get; set;}

    }


    public class KanbanizeCardSubtask
    {
        public string Description { get; set; }
        public int Id { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime FinishedAt { get; set; }

    }
}
