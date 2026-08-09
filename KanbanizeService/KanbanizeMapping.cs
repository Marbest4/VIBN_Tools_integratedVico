using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIBN_Tools.KanbanizeService
{
    public static class KanbanizeMapping
    {

        public static KanbanizeBoard MapBoard(KanbanizeBoardDto dto)
        {
            return new KanbanizeBoard
            {
                Id = dto.board_id,
                Name = dto.name,
                Description = dto.description,
                Type = dto.type,
                IsArchived = dto.is_archived == 1,
                Workflows = new List<KanbanizeWorkflow>(),
            };
        }


        public static KanbanizeWorkflow MapWorkflow(KanbanizeWorkflowDto dto)
        {
            return new KanbanizeWorkflow
            {
                Id = dto.workflow_id,
                Name = dto.name,
                Type = dto.type,
                Position = dto.position,
                Lanes = new List<KanbanizeLane>(),
                Columns = new List<KanbanizeColumn>(),
            };
        }


        public static KanbanizeLane MapLane(KanbanizeLaneDto dto)
        {
            return new KanbanizeLane
            {
                Name = dto.name,
                Id = dto.lane_id,
                Description = dto.description,
                WorkflowId = dto.workflow_id,
                Position = dto.position,
                Color = dto.color,
                Cards = new List<KanbanizeCard>()
            };
        }


        public static KanbanizeColumn MapColumn(KanbanizeColumnDto dto)
        {
            return new KanbanizeColumn
            {
                Id = dto.column_id,
                Name = dto.name,
                Description = dto.description,
                WorkflowId = dto.workflow_id,
                ParentColumnId = dto.parent_column_id,
                Section = dto.section,
                Position = dto.position,
                Color = dto.color
            };
        }


        public static KanbanizeCard MapCard(KanbanizeCardDto dto)
        {
            return new KanbanizeCard
            {
                Id = dto.card_id,
                Title = dto.title,
                BoardId = dto.board_id,
                WorkflowId = dto.workflow_id,
                LaneId = dto.lane_id,
                ColumnId = dto.column_id,
                Section = dto.section,
                Color = dto.color,
                Subtasks = new List<KanbanizeCardSubtask>()
            };
        }






    }
}
