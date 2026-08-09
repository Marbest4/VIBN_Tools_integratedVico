using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VIBN_Tools.KanbanizeService
{
    public class KanbanizeBoardListResponseDto
    {
        public List<KanbanizeBoardDto> data { get; set; }
    }

    public class KanbanizeBoardDto
    {
        public int board_id { get; set; }
        public int workspace_id { get; set; }
        public int is_archived { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int type { get; set; }
    }



    public class KanbanizeWorkflowListResponseDto
    {
        public List<KanbanizeWorkflowDto> data { get; set; }
    }

    public class KanbanizeWorkflowDto
    {
        public int workflow_id { get; set; }
        public string name { get; set; }
        public int type { get; set; }
        public int position { get; set; }
        public int is_enabled { get; set; }
        public int is_collapsible { get; set; }
    }




    public class KanbanizeLaneListResponseDto
    {
        public List<KanbanizeLaneDto> data { get; set; }
    }

    public class KanbanizeLaneDto
    {
        public int lane_id { get; set; }
        public int workflow_id { get; set; }
        public int? parent_lane_id { get; set; }
        public int position { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string color { get; set; }
    }




    public class KanbanizeColumnListResponseDto
    {
        public List<KanbanizeColumnDto> data { get; set; }
    }

    public class KanbanizeColumnDto
    {
        public int column_id { get; set; }
        public int workflow_id { get; set; }
        public int? parent_column_id { get; set; }
        public int? section { get; set; }
        public int position { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string color { get; set; }
    }




    public class KanbanizeCardListResponseDto
    {
        public KanbanizeCardListDataDto data { get; set; }
    }

    public class KanbanizeCardListDataDto
    {
        public KanbanizePaginationDto pagination { get; set; }
        public List<KanbanizeCardDto> data { get; set; }
    }

    public class KanbanizePaginationDto
    {
        public int all_pages { get; set; }
        public int current_page { get; set; }
        public int results_per_page { get; set; }
    }


    public class KanbanizeCardDto
    {
        public int card_id { get; set; }
        public string custom_id { get; set; }
        public int board_id { get; set; }
        public int workflow_id { get; set; }
        public string title { get; set; }
        public int? owner_user_id { get; set; }
        public int? type_id { get; set; }
        public string color { get; set; }
        public int? section { get; set; }
        public int column_id { get; set; }
        public int lane_id { get; set; }
        public int position { get; set; }
    }








}
