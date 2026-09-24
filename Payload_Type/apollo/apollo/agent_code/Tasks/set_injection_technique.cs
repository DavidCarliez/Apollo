#define COMMAND_NAME_UPPER

#if DEBUG
#define SET_INJECTION_TECHNIQUE
#endif

#if SET_INJECTION_TECHNIQUE

using AgInterop.Classes;
using AgInterop.Interfaces;
using AgInterop.Structs.MythicStructs;

namespace Tasks
{
    public class set_injection_technique : Tasking
    {
        public set_injection_technique(IAgent agent, AgInterop.Structs.MythicStructs.MythicTask data) : base(agent, data)
        {
        }


        public override void Start()
        {
            MythicTaskResponse resp;
            if (_agent.GetInjectionManager().SetTechnique(_data.Parameters))
            {
                resp = CreateTaskResponse($"Set injection technique to {_data.Parameters}", true);
            }
            else
            {
                resp = CreateTaskResponse($"Unknown technique: {_data.Parameters}", true, "error");
            }

            // Your code here..
            // Then add response to queue
            _agent.GetTaskManager().AddTaskResponseToQueue(resp);
        }
    }
}

#endif